##Augmented Reality World Explorer repository

AR World Explorer is an augmented reality app that provides relevant useful information about places and social events happening around the users. This information is presented in real-time, in the form of augmented reality labels, as users point their smartphones camera towards the areas of interest.

Information includes: building names, historical facts, food/shopping places, reviews, personal tagged locations, social networking events.

The system design includes a lightweight mobile client on Windows Phone, and a backend component running on Windows Azure Cloud Service (Web Role and Worker Role instances) and Azure Storage (Table, Queue and Blob storages).

1. Leveraging The Cloud: 

    Network-intensive operations such as searching for locational information of the phone using Google/Bing maps and grabbing information from social networks, streetview images and other sources will all be done on Azure, which can save battery life and lessen data usage for the mobile devices. In using the cloud, we include several optimizations such as caching, data filtering, data aggregation and result predictions.

2. Data Filtering & Aggregation: 

    Since the cloud retrieves information regarding all places and events nearby the users ahead of their requests: We can filter out and send back only details for the requested point of interest and limited number of additional interested locations. The cloud also can also make many connections to multiple information sources to gather more details for a particular location. This aggregates data and the mobile only has to make one connection to the cloud to get all the necessary information.

3. Predictions:

    Since data filtering holds back from sending extra information about other places to the mobile (improving energy/data bandwidth saving), this can hurts performance if users decide to view details on another place on screen (higher latency, waiting to fetch from the cloud). Our system tries to balance the tradeoff between Energy/Data and Performance by introducing predictions: the cloud sends back details about additional places, it tries to be smart in selecting locations that users may be interested in, based on current time, access patterns, friends list, etc.

4. GART Library:

    The GART library integrates Augmented Reality into a Windows Phone app using the Motion API shipped with Windows Phone Mango (WP7.5). GART is used to to generate the AR labels from coordinates sent from the cloud. The phone will pass location coordinates (received as part of the cloud’s JSON response for basic requests) into GART lib which will calculate the appropriate display location for the AR labels, containing place names and links to a page for information, which are made to appear on screen. The use of the GART library not only simplifies the code on the mobile side but allows to provide our application with more functionality i.e. with the different layers (food, social, personal) to filter place search results.

5. Social Integration:

   ARWorldExplorer leverages the social media information to provide a better experience for the users. The Social layer is an important part of the App, allowing users to interact with their social network through Augmented Reality interface. Facebook access token is used by Facebook apps to access user’s data without having to asking for permissions too frequently. On the mobile device, after user connects with an app using Facebook Login (managed by Facebook), the app can access user’s public information (public profile and friendlist). If an app wants to access other private or sensitive information, there will be a Login Dialog, which lets the user know which permissions he/she is going to expose to the app. After the login process is finished, the app would be provided an access_token. The access_token can be invalidated by two ways:
  * It can expire after a specific period, which could be a few hours for short-lived tokens or a few days for long-lived tokens (which can be requested with the short-lived tokens).
  * The user chooses to stop exposing information to the app. 

    On the cloud, we retrieve the users Facebook information using Facebook Query Language (FQL), which allows querying Facebook user data by using a SQL-style interface. It also allows batching multiple queries into a single call. Data returned from an FQL query is in JSON format by default. The cloud currently obtains facebook events details, friends in attendance list for each event, and friends check-ins information.


6. Scalability:

    Azure Cloud Service allows us to control the scaling level and memory usage in Azure. Web Role and Worker Role can scale independently from each other, giving us the flexibility to control resources for optimal pricing while still maintaining the service availability.
For example:
  * In case where there are many users accessing from a few locations, more Azure Web Role instances can handle the higher amount of incoming requests, while only 1 Worker Role instance is needed to process the workload of gathering the extra details.
  * In another case where there are fewer users accessing from many geographically distributed locations, then just 1 Web Role is sufficient to handle requests, but multiple Worker Role instances may be needed to get details for many distinct places/events.